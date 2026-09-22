```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  31.81 μs |  2.379 μs | 0.130 μs |  1.00 |    0.01 |    2 |  0.7324 |       - |       - |  12.02 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 148.66 μs | 22.360 μs | 1.226 μs |  4.67 |    0.04 |    4 |  3.1738 |       - |       - |  55.77 KB |        4.64 |
| &#39;JSON export: 200 activities&#39; | 704.72 μs | 27.319 μs | 1.497 μs | 22.16 |    0.09 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.30 |
| &#39;CSV export: 10 activities&#39;   |  14.52 μs |  9.072 μs | 0.497 μs |  0.46 |    0.01 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.57 |
| &#39;CSV export: 50 activities&#39;   |  62.99 μs |  2.511 μs | 0.138 μs |  1.98 |    0.01 |    3 | 14.1602 |  2.3193 |       - |    233 KB |       19.39 |
| &#39;CSV export: 200 activities&#39;  | 404.41 μs | 99.041 μs | 5.429 μs | 12.72 |    0.15 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.94 |
