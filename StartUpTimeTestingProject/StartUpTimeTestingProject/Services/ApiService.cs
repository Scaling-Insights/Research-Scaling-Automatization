using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;


public class ApiService
{
    private static readonly string vkeId = ""; //clusterID
    private static readonly string token = ""; //apiKey of Vultr
    private static readonly HttpClient client = new HttpClient();

    private static readonly bool test = true;
    private static readonly string backendImage = ""; //Image link from image registry at vultr
    private static readonly string testImage = "nginx";

    private static readonly string regularNodePlanvcp4 = "vc2-4c-8gb";
    private static readonly string AMDNodePlanvcp4 = "vhp-4c-8gb-amd";
    private static readonly string intelNodePlanvcp4 = "vhp-4c-8gb-intel";

    private static readonly string regularNodePlan = "vc2-2c-2gb";
    private static readonly string AMDNodePlan = "vhp-2c-2gb-amd";
    private static readonly string intelNodePlan = "vhp-2c-2gb-intel";
    private static string nodePlan => intelNodePlan;

    public static async Task TestProcess(int poolCount, int nodeCount)
    {
        Console.WriteLine("Starting TestProcess");
        Stopwatch sw = Stopwatch.StartNew();
        var poolsTaskStack = await MultiplePoolCreation(poolCount, nodeCount);
        var poolsStack = new Stack<NodePoolResponse>();
        while (poolsTaskStack.Count > 0)
        {
            poolsStack.Push(await poolsTaskStack.Pop());
        }
        int countNotActivePools = 1;
        GetAllNodePoolResponseDto poolsData = null;
        while (countNotActivePools > 0)
        {
            if (poolsData != null) Thread.Sleep(1000);
            poolsData = await GetAllPools();
            countNotActivePools = poolsData.node_pools.Count(p => p.status != "active");
        }
        Console.WriteLine($"Pool creation in {sw.ElapsedMilliseconds} ms");

        List<Node> nodes = new List<Node>();
        int countNotActiveNodes = 1;
        while (countNotActiveNodes > 0)
        {
            if (poolsData != null) Thread.Sleep(1000);
            poolsData = await GetAllPools();
            countNotActiveNodes = poolsData.node_pools.Where(e => e.label == "test-ov-automatisering").Sum(e => e.nodes.Count(n => n.status != "active"));
        }
        foreach (var nodePool in poolsStack)
        {
            foreach (var node in nodePool.node_pool.nodes)
            {
                while (!await InstanceIsOk(node.id))
                {
                    Thread.Sleep(1000);
                }
                ;
            }
        }
        sw.Stop();
        Console.WriteLine($"Node creation in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine("Delete pools?");
        Console.ReadLine();
        sw.Start();
        sw.Stop();
        sw.Start();
        int currentPoolCount = 2 + poolCount;
        while (poolsStack.Count > 0)
        {
            await DeleteNodePoolsAsync(poolsStack.Pop().node_pool.id);
        }
        foreach (var nodePool in poolsStack)
        {
            foreach (var node in nodePool.node_pool.nodes)
            {
                await DeleteNodeAsync(node.id);
            }
        }
        Console.WriteLine($"Nodes Deleted in {sw.ElapsedMilliseconds} ms");
        while (currentPoolCount > 2)
        {
            Thread.Sleep(1000);
            var temp = await GetAllPools();
            currentPoolCount = temp.meta.total;
        }
        sw.Stop();
        Console.WriteLine($"Pool Deleted in {sw.ElapsedMilliseconds} ms");
    }

    public static async Task DeleteNodeAsync(string nodeId)
    {
        string url = $"https://api.vultr.com/v2/instances/{nodeId}";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            HttpResponseMessage response = await client.DeleteAsync(url);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Success: {response.StatusCode}");
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                string errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Error details: " + errorResponse);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception occurred: " + ex.Message);
        }
    }
    public static async Task<GetAllNodePoolResponseDto> GetAllPools()
    {
        string url = $"https://api.vultr.com/v2/kubernetes/clusters/{vkeId}/node-pools";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GetAllNodePoolResponseDto>(responseBody);
                //Console.WriteLine("Response: " + responseBody);
                return result;
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                string errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Error details: " + errorResponse);
                return null;

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception occurred: " + ex.Message);
            return null;
        }
    }
    public static async Task<Stack<Task<NodePoolResponse>>> MultiplePoolCreation(int poolCount, int nodeCount)
    {
        Stack<Task<NodePoolResponse>> pools = new Stack<Task<NodePoolResponse>>();
        for (int i = 0; i < poolCount; i++)
        {
            pools.Push(PostNodePoolAsync(nodeCount, i + 1));
        }
        return pools;
    }
    public static async Task<NodePoolResponse> PostNodePoolAsync(int quantity, int poolcount)
    {
        string url = $"https://api.vultr.com/v2/kubernetes/clusters/{vkeId}/node-pools";

        using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                node_quantity = quantity,
                label = $"nodepool-{poolcount}",
                plan = nodePlan,
                tag = "test-ov-automatisering",
                min_nodes = 1,
                max_nodes = 4,
                auto_scaler = false
            }),
            Encoding.UTF8,
            "application/json");
        Console.WriteLine(await jsonContent.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            HttpResponseMessage response = await client.PostAsync(url, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<NodePoolResponse>(responseBody);
                //Console.WriteLine("Response: " + responseBody);
                return result;
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                string errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Error details: " + errorResponse);
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception occurred: " + ex.Message);
            return null;
        }
    }

    public static async Task GetPlans()
    {
        string url = "https://api.vultr.com/v2/regions/ams/availability";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response body: " + responseBody);
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                string errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Error details: " + errorResponse);
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception occurred: " + ex.Message);
        }
    }
    public static async Task DeleteNodePoolsAsync(string poolId)
    {
        string url = $"https://api.vultr.com/v2/kubernetes/clusters/{vkeId}/node-pools/{poolId}";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        try
        {
            HttpResponseMessage response = await client.DeleteAsync(url);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Success: {response.StatusCode}");
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                string errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Error details: " + errorResponse);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception occurred: " + ex.Message);
        }
    }



    // Niet meer nodig
    public static void CreateDeployment(int count)
    {
        string imageName;
        if (test) imageName = testImage;
        else imageName = backendImage;
        RunCmd($"kubectl create deployment test-automatisering --replicas={count} --image={imageName}");
    }

    public static void DeleteDeployment()
    {
        RunCmd($"kubectl delete deployment test-automatisering");
    }
    private static void RunCmd(string command)
    {
        Process process = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = "cmd.exe";
        startInfo.Arguments = $"/C {command}";
        process.StartInfo = startInfo;
        process.Start();
    }

    private static void RunTerminal(string command)
    {
        Process process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
    }

    public static async Task<bool> InstanceIsOk(string id)
    {
        string url = $"https://api.vultr.com/v2/instances/{id}";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            string responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<InstanceResponseDto>(responseBody);
            return result.instance.server_status == "ok";
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception occurred: " + ex.Message);
            return false;
        }
    }
}