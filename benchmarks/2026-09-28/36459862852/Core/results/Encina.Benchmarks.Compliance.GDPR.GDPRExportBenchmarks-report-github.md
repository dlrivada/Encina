```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  36.23 μs |   2.585 μs | 0.142 μs |  1.00 |    0.00 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 175.05 μs |  12.035 μs | 0.660 μs |  4.83 |    0.02 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 779.14 μs |  46.685 μs | 2.559 μs | 21.51 |    0.10 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  16.04 μs |   4.146 μs | 0.227 μs |  0.44 |    0.01 |    1 |  3.3569 |  0.2136 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  75.33 μs |   7.539 μs | 0.413 μs |  2.08 |    0.01 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 434.61 μs | 140.807 μs | 7.718 μs | 12.00 |    0.19 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
