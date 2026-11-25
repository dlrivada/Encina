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
| &#39;JSON export: 10 activities&#39;  |  22.80 μs |   0.171 μs | 0.009 μs |  1.00 |    0.00 |    2 |  0.1221 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 106.51 μs |   1.175 μs | 0.064 μs |  4.67 |    0.00 |    4 |  0.6104 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 470.56 μs |   4.659 μs | 0.255 μs | 20.64 |    0.01 |    6 | 52.2461 | 52.2461 | 52.2461 | 219.51 KB |       18.21 |
| &#39;CSV export: 10 activities&#39;   |  11.44 μs |   0.637 μs | 0.035 μs |  0.50 |    0.00 |    1 |  0.6714 |  0.0305 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  52.54 μs |   8.923 μs | 0.489 μs |  2.30 |    0.02 |    3 |  2.8076 |  0.4883 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 268.68 μs | 113.307 μs | 6.211 μs | 11.78 |    0.24 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
