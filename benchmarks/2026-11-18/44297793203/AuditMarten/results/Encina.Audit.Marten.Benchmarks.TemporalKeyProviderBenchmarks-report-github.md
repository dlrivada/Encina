```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   163.9 ns |    29.64 ns |  1.62 ns |  1.20 |    0.01 |    1 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   116.9 ns |    24.52 ns |  1.34 ns |  0.85 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,724.0 ns |   398.95 ns | 21.87 ns | 19.88 |    0.15 |    2 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,020.1 ns | 1,553.32 ns | 85.14 ns | 29.34 |    0.55 |    3 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   137.0 ns |     9.62 ns |  0.53 ns |  1.00 |    0.00 |    1 | 0.0210 |      - |     352 B |        1.00 |
