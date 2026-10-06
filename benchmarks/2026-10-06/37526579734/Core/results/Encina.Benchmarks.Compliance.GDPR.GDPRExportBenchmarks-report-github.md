```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  34.61 μs |   0.584 μs | 0.032 μs |  1.00 |    0.00 |    2 |  0.4883 |       - |       - |  12.03 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 163.55 μs |   2.039 μs | 0.112 μs |  4.73 |    0.00 |    4 |  2.1973 |       - |       - |  55.77 KB |        4.64 |
| &#39;JSON export: 200 activities&#39; | 749.75 μs | 102.146 μs | 5.599 μs | 21.67 |    0.14 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.28 |
| &#39;CSV export: 10 activities&#39;   |  17.43 μs |   7.581 μs | 0.416 μs |  0.50 |    0.01 |    1 |  2.2278 |  0.1221 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  76.39 μs |  27.538 μs | 1.509 μs |  2.21 |    0.04 |    3 |  9.3994 |  1.7090 |       - | 232.99 KB |       19.37 |
| &#39;CSV export: 200 activities&#39;  | 426.42 μs | 105.510 μs | 5.783 μs | 12.32 |    0.15 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.84 |
