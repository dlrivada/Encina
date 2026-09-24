```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 138.205 ns |  15.705 ns |  0.8608 ns | 16.85 |    0.14 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.204 ns |   1.152 ns |  0.0632 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 581.366 ns |  56.541 ns |  3.0992 ns | 70.87 |    0.57 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 542.586 ns | 270.016 ns | 14.8005 ns | 66.14 |    1.62 | 0.0601 |    1008 B |       42.00 |
