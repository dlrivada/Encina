```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  21.56 μs |  29.090 μs |  1.595 μs |  1.00 |    0.09 |    2 |  0.1221 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 106.58 μs | 144.558 μs |  7.924 μs |  4.96 |    0.44 |    4 |  0.6104 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 443.06 μs |  33.663 μs |  1.845 μs | 20.62 |    1.27 |    6 | 52.2461 | 52.2461 | 52.2461 | 219.51 KB |       18.21 |
| &#39;CSV export: 10 activities&#39;   |  13.26 μs |   8.401 μs |  0.460 μs |  0.62 |    0.04 |    1 |  0.6714 |  0.0305 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  55.22 μs |  50.359 μs |  2.760 μs |  2.57 |    0.19 |    3 |  2.8076 |  0.4883 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 312.94 μs | 499.793 μs | 27.395 μs | 14.57 |    1.42 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
