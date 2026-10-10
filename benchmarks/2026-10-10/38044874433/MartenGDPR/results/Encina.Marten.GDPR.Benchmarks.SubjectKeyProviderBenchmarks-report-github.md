```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **212.86 ns** |     **6.495 ns** |   **0.356 ns** |  **1.00** |    **0.00** |    **2** | **0.0134** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,678.69 ns | 4,096.223 ns | 224.528 ns | 17.31 |    0.92 |    3 | 0.0343 | 0.0305 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    98.60 ns |     9.800 ns |   0.537 ns |  0.46 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   212.46 ns |     7.495 ns |   0.411 ns |  1.00 |    0.00 |    2 | 0.0129 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **206.91 ns** |    **17.962 ns** |   **0.985 ns** |  **0.94** |    **0.00** |    **2** | **0.0134** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,679.59 ns | 3,050.186 ns | 167.191 ns | 16.69 |    0.66 |    3 | 0.0343 | 0.0305 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    94.08 ns |     2.233 ns |   0.122 ns |  0.43 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   220.44 ns |    12.990 ns |   0.712 ns |  1.00 |    0.00 |    2 | 0.0129 |      - |     328 B |        1.00 |
