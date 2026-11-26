```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|-----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  28.63 μs |   0.533 μs |  0.029 μs |  1.00 |    0.00 |    2 |  0.1221 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 140.74 μs |  23.251 μs |  1.274 μs |  4.92 |    0.04 |    4 |  0.4883 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 628.11 μs |  35.968 μs |  1.972 μs | 21.94 |    0.06 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  17.59 μs |   2.109 μs |  0.116 μs |  0.61 |    0.00 |    1 |  0.6714 |  0.0305 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  78.13 μs |   1.724 μs |  0.094 μs |  2.73 |    0.00 |    3 |  2.8076 |  0.4883 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 401.92 μs | 265.781 μs | 14.568 μs | 14.04 |    0.44 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
