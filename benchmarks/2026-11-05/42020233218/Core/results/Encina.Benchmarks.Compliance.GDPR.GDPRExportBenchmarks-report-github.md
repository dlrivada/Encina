```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  36.08 μs |   2.965 μs |  0.163 μs |  1.00 |    0.01 |    2 |  0.4883 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 162.25 μs |   2.709 μs |  0.148 μs |  4.50 |    0.02 |    4 |  2.1973 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 754.06 μs |  36.800 μs |  2.017 μs | 20.90 |    0.09 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  18.68 μs |   1.155 μs |  0.063 μs |  0.52 |    0.00 |    1 |  2.2278 |  0.1221 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  81.74 μs |  12.422 μs |  0.681 μs |  2.27 |    0.02 |    3 |  9.3994 |  1.7090 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 496.99 μs | 242.475 μs | 13.291 μs | 13.77 |    0.32 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
