```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|---------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  24.81 μs |   1.424 μs | 0.078 μs |  1.00 |    0.00 |    2 |  0.1221 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 118.77 μs |  31.379 μs | 1.720 μs |  4.79 |    0.06 |    4 |  0.6104 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 532.91 μs |  44.761 μs | 2.454 μs | 21.48 |    0.10 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  12.85 μs |   1.483 μs | 0.081 μs |  0.52 |    0.00 |    1 |  0.6714 |  0.0305 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  59.36 μs |   9.134 μs | 0.501 μs |  2.39 |    0.02 |    3 |  2.8076 |  0.4883 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 331.98 μs | 102.374 μs | 5.611 μs | 13.38 |    0.20 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
