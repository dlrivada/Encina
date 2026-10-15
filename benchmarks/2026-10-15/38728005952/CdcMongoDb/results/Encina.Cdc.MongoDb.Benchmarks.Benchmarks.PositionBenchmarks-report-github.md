```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 141.065 ns |  20.8140 ns |  1.1409 ns | 16.27 |    0.13 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.670 ns |   0.6619 ns |  0.0363 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 604.327 ns |  88.8240 ns |  4.8687 ns | 69.70 |    0.55 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 570.139 ns | 209.5751 ns | 11.4875 ns | 65.76 |    1.17 | 0.0601 |    1008 B |       42.00 |
