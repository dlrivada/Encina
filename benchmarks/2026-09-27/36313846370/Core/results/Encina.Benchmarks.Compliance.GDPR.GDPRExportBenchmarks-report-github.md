```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  36.05 μs |  2.579 μs | 0.141 μs |  1.00 |    0.00 |    2 |  0.7324 |       - |       - |  12.03 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 172.37 μs |  3.365 μs | 0.184 μs |  4.78 |    0.02 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.64 |
| &#39;JSON export: 200 activities&#39; | 788.46 μs | 70.174 μs | 3.846 μs | 21.87 |    0.12 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.28 |
| &#39;CSV export: 10 activities&#39;   |  14.81 μs |  1.109 μs | 0.061 μs |  0.41 |    0.00 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  65.84 μs |  3.160 μs | 0.173 μs |  1.83 |    0.01 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.37 |
| &#39;CSV export: 200 activities&#39;  | 398.15 μs | 61.625 μs | 3.378 μs | 11.04 |    0.09 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.84 |
