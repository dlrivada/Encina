```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |-----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  19.212 μs |  16.219 μs | 0.8890 μs |  1.00 |    0.06 |    2 |  0.7324 |       - |       - |  12.03 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  |  85.862 μs |  11.036 μs | 0.6049 μs |  4.48 |    0.19 |    4 |  3.2959 |       - |       - |  55.77 KB |        4.64 |
| &#39;JSON export: 200 activities&#39; | 412.189 μs |  44.573 μs | 2.4432 μs | 21.49 |    0.89 |    6 | 55.1758 | 55.1758 | 55.1758 | 219.91 KB |       18.28 |
| &#39;CSV export: 10 activities&#39;   |   8.330 μs |   7.824 μs | 0.4288 μs |  0.43 |    0.03 |    1 |  3.3569 |  0.2289 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  35.718 μs |   5.496 μs | 0.3013 μs |  1.86 |    0.08 |    3 | 14.2212 |  2.3193 |       - |    233 KB |       19.37 |
| &#39;CSV export: 200 activities&#39;  | 256.655 μs | 111.810 μs | 6.1287 μs | 13.38 |    0.62 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.84 |
