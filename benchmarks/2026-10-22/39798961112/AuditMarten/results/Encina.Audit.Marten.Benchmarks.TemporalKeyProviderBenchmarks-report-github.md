```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | PeriodCount | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   133.77 ns |   1.118 ns |   1.567 ns |  0.99 |    0.02 |    2 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    82.26 ns |   0.313 ns |   0.438 ns |  0.61 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,613.79 ns |  16.864 ns |  24.186 ns | 19.40 |    0.28 |    3 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,158.73 ns | 138.399 ns | 194.017 ns | 23.44 |    1.44 |    4 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   134.75 ns |   1.057 ns |   1.516 ns |  1.00 |    0.02 |    2 | 0.0041 |      - |     352 B |        1.00 |
