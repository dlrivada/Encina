```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 135.525 ns | 0.5360 ns | 0.7857 ns | 15.56 |    0.60 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.725 ns | 0.2324 ns | 0.3479 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 619.409 ns | 6.6583 ns | 9.7596 ns | 71.10 |    2.92 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 555.310 ns | 6.6447 ns | 9.7397 ns | 63.74 |    2.66 | 0.0601 |    1008 B |       42.00 |
