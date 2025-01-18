using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class NodePoolResponse
{
    public NodePool node_pool { get; set; }
}
public class GetAllNodePoolResponseDto
{
    public List<NodePool> node_pools { get; set; }
    public Meta meta { get; set; }
}
public class NodePool
{
    public string id { get; set; }
    public DateTime date_created { get; set; }
    public DateTime date_updated { get; set; }
    public string label { get; set; }
    public string tag { get; set; }
    public string plan { get; set; }
    public string status { get; set; }
    public int node_quantity { get; set; }
    public int min_nodes { get; set; }
    public int max_nodes { get; set; }
    public bool auto_scaler { get; set; }
    public List<Node> nodes { get; set; }
}

public class Node
{
    public string id { get; set; }
    public string label { get; set; }
    public DateTime date_created { get; set; }
    public string status { get; set; }
}
public class Meta
{
    public int total { get; set; }
    public Links links { get; set; }
}

public class Links
{
    public string next { get; set; }
    public string prev { get; set; }
}