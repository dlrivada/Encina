```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  24.76 μs |   3.849 μs |  0.211 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 115.31 μs |  14.513 μs |  0.796 μs |  4.66 |    0.04 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 555.85 μs | 159.566 μs |  8.746 μs | 22.45 |    0.35 |    6 | 51.7578 | 51.7578 | 51.7578 | 219.51 KB |       18.21 |
| &#39;CSV export: 10 activities&#39;   |  13.67 μs |   3.023 μs |  0.166 μs |  0.55 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  56.62 μs |  23.551 μs |  1.291 μs |  2.29 |    0.05 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 377.09 μs | 194.540 μs | 10.663 μs | 15.23 |    0.39 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
