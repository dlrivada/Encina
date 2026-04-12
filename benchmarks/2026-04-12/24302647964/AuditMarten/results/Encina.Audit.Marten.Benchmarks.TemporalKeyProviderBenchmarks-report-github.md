```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | PeriodCount | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   163.2 ns |  0.44 ns |  0.64 ns |  1.12 |    0.05 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   110.5 ns |  0.18 ns |  0.25 ns |  0.76 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,666.4 ns | 25.07 ns | 36.75 ns | 18.33 |    0.83 |    4 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,392.3 ns | 61.46 ns | 84.13 ns | 30.20 |    1.42 |    5 | 0.0458 | 0.0381 |     848 B |        2.41 |
| GetExistingKey         | 12          |   145.7 ns |  4.28 ns |  6.41 ns |  1.00 |    0.06 |    2 | 0.0210 |      - |     352 B |        1.00 |
