//var result = await ApiService.GetAllPools();

//ApiService.DeleteNodeAsync(result.node_pools[2].nodes[0].id,result.node_pools[2].id);
//ApiService.DeleteNodePoolsAsync(result.node_pools[1].id);

//await ApiService.InstanceIsOk(result.node_pools[2].nodes[0].id);

//await ApiService.GetPlans();

//await ApiService.CreateCluster("3");

//await ApiService.GetK8Config();

await ApiService.TestProcess(2,1);