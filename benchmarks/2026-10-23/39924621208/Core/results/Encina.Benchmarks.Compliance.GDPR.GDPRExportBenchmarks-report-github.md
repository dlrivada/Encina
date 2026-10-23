```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  35.38 μs |   4.531 μs |  0.248 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.03 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 170.74 μs |  27.536 μs |  1.509 μs |  4.83 |    0.05 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.64 |
| &#39;JSON export: 200 activities&#39; | 776.34 μs |  30.311 μs |  1.661 μs | 21.94 |    0.14 |    6 | 51.7578 | 51.7578 | 51.7578 | 219.51 KB |       18.25 |
| &#39;CSV export: 10 activities&#39;   |  16.08 μs |   4.027 μs |  0.221 μs |  0.45 |    0.01 |    1 |  3.3569 |  0.2136 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  71.95 μs |  15.936 μs |  0.873 μs |  2.03 |    0.02 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.37 |
| &#39;CSV export: 200 activities&#39;  | 467.89 μs | 362.966 μs | 19.895 μs | 13.22 |    0.49 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.84 |
