
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                                    | ShardCount | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------------ |----------- |----------:|------:|------:|-----:|----------:|------------:|
 **'Scatter-gather all shards (sync result)'** | **3**          | **40.163 ms** |    **NA** |  **1.00** |    **5** |    **3648 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 3          | 39.263 ms |    NA |  0.98 |    3 |    3704 B |        1.02 |
 'Scatter-gather with large results'       | 3          | 40.124 ms |    NA |  1.00 |    4 |   28704 B |        7.87 |
 'Scatter-gather single shard'             | 3          | 38.884 ms |    NA |  0.97 |    2 |    2328 B |        0.64 |
 'Topology lookup all shards'              | 3          |  1.126 ms |    NA |  0.03 |    1 |     104 B |        0.03 |
                                           |            |           |       |       |      |           |             |
 **'Scatter-gather all shards (sync result)'** | **25**         | **39.103 ms** |    **NA** |  **1.00** |    **3** |   **19464 B** |        **1.00** |
 'Scatter-gather subset (3 shards)'        | 25         | 39.000 ms |    NA |  1.00 |    2 |   16216 B |        0.83 |
 'Scatter-gather with large results'       | 25         | 41.877 ms |    NA |  1.07 |    5 |  242952 B |       12.48 |
 'Scatter-gather single shard'             | 25         | 40.148 ms |    NA |  1.03 |    4 |    2504 B |        0.13 |
 'Topology lookup all shards'              | 25         |  1.131 ms |    NA |  0.03 |    1 |     280 B |        0.01 |
