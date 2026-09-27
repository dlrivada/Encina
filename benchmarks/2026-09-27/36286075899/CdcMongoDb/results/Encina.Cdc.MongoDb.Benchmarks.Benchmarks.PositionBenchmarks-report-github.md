```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 117.246 ns | 0.9628 ns | 1.3497 ns | 17.94 |    0.96 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   6.552 ns | 0.2394 ns | 0.3510 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 490.363 ns | 5.2342 ns | 7.8343 ns | 75.04 |    4.09 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 471.620 ns | 5.1584 ns | 7.7208 ns | 72.17 |    3.94 | 0.0601 |    1008 B |       42.00 |
