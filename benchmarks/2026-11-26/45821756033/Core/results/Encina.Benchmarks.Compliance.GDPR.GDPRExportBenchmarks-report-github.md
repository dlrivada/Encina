```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  22.51 μs |   0.792 μs | 0.043 μs |  1.00 |    0.00 |    2 |  0.1221 |       - |       - |  12.02 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 107.70 μs |   1.261 μs | 0.069 μs |  4.78 |    0.01 |    4 |  0.6104 |       - |       - |  55.77 KB |        4.64 |
| &#39;JSON export: 200 activities&#39; | 476.24 μs |  39.209 μs | 2.149 μs | 21.15 |    0.09 |    6 | 55.1758 | 55.1758 | 55.1758 | 219.91 KB |       18.30 |
| &#39;CSV export: 10 activities&#39;   |  11.81 μs |   2.385 μs | 0.131 μs |  0.52 |    0.01 |    1 |  0.6714 |  0.0305 |       - |  54.92 KB |        4.57 |
| &#39;CSV export: 50 activities&#39;   |  53.70 μs |   7.226 μs | 0.396 μs |  2.39 |    0.02 |    3 |  2.8076 |  0.4883 |       - | 232.99 KB |       19.39 |
| &#39;CSV export: 200 activities&#39;  | 273.74 μs | 115.704 μs | 6.342 μs | 12.16 |    0.24 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.94 |
