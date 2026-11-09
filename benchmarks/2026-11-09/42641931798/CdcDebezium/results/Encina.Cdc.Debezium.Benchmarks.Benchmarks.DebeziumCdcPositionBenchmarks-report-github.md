```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition | 10.88 ns |  1.423 ns | 0.078 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 59.12 ns | 13.599 ns | 0.745 ns |  5.44 |    0.07 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 46.96 ns | 26.781 ns | 1.468 ns |  4.32 |    0.12 | 0.0091 |     152 B |        6.33 |
