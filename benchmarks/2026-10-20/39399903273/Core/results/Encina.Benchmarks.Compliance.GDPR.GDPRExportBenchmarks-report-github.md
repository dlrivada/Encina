```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  35.31 μs |   1.453 μs |  0.080 μs |  1.00 |    0.00 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 173.33 μs |  57.946 μs |  3.176 μs |  4.91 |    0.08 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 782.60 μs |  72.951 μs |  3.999 μs | 22.17 |    0.11 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  15.51 μs |   1.650 μs |  0.090 μs |  0.44 |    0.00 |    1 |  3.3569 |  0.2136 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  67.61 μs |   6.263 μs |  0.343 μs |  1.91 |    0.01 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 408.12 μs | 294.959 μs | 16.168 μs | 11.56 |    0.40 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
