```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  24.27 μs |  2.629 μs | 0.144 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 114.52 μs |  2.471 μs | 0.135 μs |  4.72 |    0.02 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 537.59 μs | 31.233 μs | 1.712 μs | 22.15 |    0.13 |    6 | 51.7578 | 51.7578 | 51.7578 | 219.51 KB |       18.21 |
| &#39;CSV export: 10 activities&#39;   |  11.26 μs |  1.413 μs | 0.077 μs |  0.46 |    0.00 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  51.70 μs | 51.798 μs | 2.839 μs |  2.13 |    0.10 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 325.08 μs | 53.012 μs | 2.906 μs | 13.40 |    0.12 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
