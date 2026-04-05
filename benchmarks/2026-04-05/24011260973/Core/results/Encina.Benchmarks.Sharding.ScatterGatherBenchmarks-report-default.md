
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                                    | ShardCount | Mean        | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------------ |----------- |------------:|------:|------:|-----:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          | **36,365.7 μs** |    **NA** |  **1.00** |    **3** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          | 35,862.4 μs |    NA |  0.99 |    2 |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 37,912.6 μs |    NA |  1.04 |    5 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          | 36,493.5 μs |    NA |  1.00 |    4 |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |  1,001.5 μs |    NA |  0.03 |    1 |     104 B |        0.03 |
                                           |            |             |       |       |      |           |             |
 **'Scatter-gather all shards (sync result)'** | **10**         | **36,728.5 μs** |    **NA** |  **1.00** |    **4** |    **8904 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 10         | 35,884.0 μs |    NA |  0.98 |    2 |    3760 B |        0.42 |
 'Scatter-gather with large results'       | 10         | 37,811.6 μs |    NA |  1.03 |    5 |   99048 B |       11.12 |
 'Scatter-gather single shard'             | 10         | 36,023.4 μs |    NA |  0.98 |    3 |    2384 B |        0.27 |
 'Topology lookup all shards'              | 10         |    999.8 μs |    NA |  0.03 |    1 |     160 B |        0.02 |
                                           |            |             |       |       |      |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         | **36,869.7 μs** |    **NA** |  **1.00** |    **4** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         | 36,782.6 μs |    NA |  1.00 |    3 |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 38,467.6 μs |    NA |  1.04 |    5 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         | 36,547.7 μs |    NA |  0.99 |    2 |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |    981.9 μs |    NA |  0.03 |    1 |     280 B |        0.01 |
