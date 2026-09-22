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
| &#39;JSON export: 10 activities&#39;  |  34.91 μs |   2.504 μs | 0.137 μs |  1.00 |    0.00 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 169.63 μs |  10.795 μs | 0.592 μs |  4.86 |    0.02 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 763.39 μs |  15.519 μs | 0.851 μs | 21.87 |    0.08 |    6 | 51.7578 | 51.7578 | 51.7578 | 219.51 KB |       18.21 |
| &#39;CSV export: 10 activities&#39;   |  15.15 μs |   4.599 μs | 0.252 μs |  0.43 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  68.09 μs |  21.808 μs | 1.195 μs |  1.95 |    0.03 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 407.98 μs | 101.694 μs | 5.574 μs | 11.69 |    0.14 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
