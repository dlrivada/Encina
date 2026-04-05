
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                                    | ShardCount | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------------ |----------- |----------:|------:|------:|-----:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          | **39.339 ms** |    **NA** |  **1.00** |    **5** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          | 37.732 ms |    NA |  0.96 |    2 |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 38.868 ms |    NA |  0.99 |    4 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          | 37.964 ms |    NA |  0.97 |    3 |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |  1.027 ms |    NA |  0.03 |    1 |     104 B |        0.03 |
                                           |            |           |       |       |      |           |             |
 **'Scatter-gather all shards (sync result)'** | **10**         | **39.329 ms** |    **NA** |  **1.00** |    **4** |    **8904 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 10         | 37.696 ms |    NA |  0.96 |    3 |    3760 B |        0.42 |
 'Scatter-gather with large results'       | 10         | 39.697 ms |    NA |  1.01 |    5 |   99048 B |       11.12 |
 'Scatter-gather single shard'             | 10         | 37.379 ms |    NA |  0.95 |    2 |    2384 B |        0.27 |
 'Topology lookup all shards'              | 10         |  1.089 ms |    NA |  0.03 |    1 |     160 B |        0.02 |
                                           |            |           |       |       |      |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         | **38.505 ms** |    **NA** |  **1.00** |    **4** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         | 37.097 ms |    NA |  0.96 |    2 |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 41.399 ms |    NA |  1.08 |    5 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         | 38.188 ms |    NA |  0.99 |    3 |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |  1.057 ms |    NA |  0.03 |    1 |     280 B |        0.01 |
