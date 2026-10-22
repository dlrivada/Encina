```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  17.294 μs |  3.4245 μs | 0.1877 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  81.277 μs |  5.9459 μs | 0.3259 μs |  4.70 |    0.05 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 401.760 μs | 49.5714 μs | 2.7172 μs | 23.23 |    0.26 |    6 | 52.2461 | 52.2461 | 52.2461 | 219.51 KB |       18.21 |
| &#39;CSV export: 10 activities&#39;   |   7.888 μs |  0.8892 μs | 0.0487 μs |  0.46 |    0.00 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  33.348 μs |  5.1317 μs | 0.2813 μs |  1.93 |    0.02 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 245.859 μs | 72.5938 μs | 3.9791 μs | 14.22 |    0.24 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
