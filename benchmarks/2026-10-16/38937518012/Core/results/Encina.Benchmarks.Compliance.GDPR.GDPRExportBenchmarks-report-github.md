```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  17.166 μs |  1.6742 μs | 0.0918 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  81.856 μs | 59.2995 μs | 3.2504 μs |  4.77 |    0.17 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 384.971 μs | 63.7306 μs | 3.4933 μs | 22.43 |    0.20 |    6 | 52.2461 | 52.2461 | 52.2461 | 219.51 KB |       18.21 |
| &#39;CSV export: 10 activities&#39;   |   7.214 μs |  0.4292 μs | 0.0235 μs |  0.42 |    0.00 |    1 |  3.3569 |  0.2365 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  31.813 μs |  6.1152 μs | 0.3352 μs |  1.85 |    0.02 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 222.621 μs | 70.5225 μs | 3.8656 μs | 12.97 |    0.20 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
