```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   155.15 ns |    33.003 ns |   1.809 ns |  0.92 |    0.01 |    2 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    95.28 ns |     8.345 ns |   0.457 ns |  0.57 |    0.00 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,916.43 ns |   421.412 ns |  23.099 ns | 17.34 |    0.12 |    3 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,341.44 ns | 3,473.583 ns | 190.399 ns | 19.87 |    0.98 |    3 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   168.16 ns |     4.691 ns |   0.257 ns |  1.00 |    0.00 |    2 | 0.0041 |      - |     352 B |        1.00 |
