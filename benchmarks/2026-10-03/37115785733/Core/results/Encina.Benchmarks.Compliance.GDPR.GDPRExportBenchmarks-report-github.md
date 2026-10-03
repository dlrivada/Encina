```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  17.962 μs |  4.3441 μs | 0.2381 μs |  1.00 |    0.02 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  85.443 μs | 11.5868 μs | 0.6351 μs |  4.76 |    0.06 |    4 |  3.2959 |       - |       - |  55.67 KB |        4.62 |
| &#39;JSON export: 200 activities&#39; | 418.301 μs | 26.4833 μs | 1.4516 μs | 23.29 |    0.28 |    6 | 55.1758 | 55.1758 | 55.1758 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |   7.884 μs |  1.4139 μs | 0.0775 μs |  0.44 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  34.984 μs |  0.5891 μs | 0.0323 μs |  1.95 |    0.02 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 239.457 μs | 14.4490 μs | 0.7920 μs | 13.33 |    0.16 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
