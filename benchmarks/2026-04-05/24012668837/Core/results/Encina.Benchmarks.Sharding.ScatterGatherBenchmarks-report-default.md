
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                                    | ShardCount | Mean        | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------------ |----------- |------------:|------:|------:|-----:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          | **38,161.7 μs** |    **NA** |  **1.00** |    **4** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          | 37,740.6 μs |    NA |  0.99 |    2 |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 39,045.8 μs |    NA |  1.02 |    5 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          | 37,857.9 μs |    NA |  0.99 |    3 |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |  1,018.8 μs |    NA |  0.03 |    1 |     104 B |        0.03 |
                                           |            |             |       |       |      |           |             |
 **'Scatter-gather all shards (sync result)'** | **10**         | **36,967.2 μs** |    **NA** |  **1.00** |    **4** |    **8904 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 10         | 36,357.4 μs |    NA |  0.98 |    2 |    3760 B |        0.42 |
 'Scatter-gather with large results'       | 10         | 38,367.3 μs |    NA |  1.04 |    5 |   99048 B |       11.12 |
 'Scatter-gather single shard'             | 10         | 36,564.2 μs |    NA |  0.99 |    3 |    2384 B |        0.27 |
 'Topology lookup all shards'              | 10         |  1,025.0 μs |    NA |  0.03 |    1 |     160 B |        0.02 |
                                           |            |             |       |       |      |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         | **37,432.2 μs** |    **NA** |  **1.00** |    **4** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         | 36,745.6 μs |    NA |  0.98 |    2 |    3880 B |        0.20 |
 'Scatter-gather with large results'       | 25         | 39,577.4 μs |    NA |  1.06 |    5 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         | 37,201.8 μs |    NA |  0.99 |    3 |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |    997.8 μs |    NA |  0.03 |    1 |     280 B |        0.01 |
