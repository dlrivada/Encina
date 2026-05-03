```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean      | Error    | StdDev    | Median    | Ratio | RatioSD | Rank | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|------------------------------ |----------:|---------:|----------:|----------:|------:|--------:|-----:|--------:|--------:|--------:|----------:|------------:|
| &#39;JSON export: 10 activities&#39;  |  34.38 μs | 0.067 μs |  0.097 μs |  34.38 μs |  1.00 |    0.00 |    2 |  0.4883 |       - |       - |  12.05 KB |        1.00 |
| &#39;JSON export: 50 activities&#39;  | 165.60 μs | 2.184 μs |  3.061 μs | 163.27 μs |  4.82 |    0.09 |    4 |  2.1973 |       - |       - |  55.77 KB |        4.63 |
| &#39;JSON export: 200 activities&#39; | 733.39 μs | 8.619 μs | 12.361 μs | 734.34 μs | 21.33 |    0.36 |    6 | 54.6875 | 54.6875 | 54.6875 | 219.91 KB |       18.24 |
| &#39;CSV export: 10 activities&#39;   |  17.69 μs | 0.140 μs |  0.210 μs |  17.69 μs |  0.51 |    0.01 |    1 |  2.2278 |  0.1221 |       - |  54.92 KB |        4.56 |
| &#39;CSV export: 50 activities&#39;   |  78.43 μs | 0.521 μs |  0.763 μs |  78.42 μs |  2.28 |    0.02 |    3 |  9.3994 |  1.7090 |       - | 232.99 KB |       19.33 |
| &#39;CSV export: 200 activities&#39;  | 427.51 μs | 3.724 μs |  5.574 μs | 427.30 μs | 12.44 |    0.16 |    5 | 49.8047 | 49.8047 | 49.8047 | 912.45 KB |       75.69 |
