```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.289 ns | 0.0047 ns | 0.0069 ns |  0.19 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.663 ns | 0.1457 ns | 0.2090 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.104 ns | 0.0717 ns | 0.1074 ns |  1.07 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.282 ns | 0.1593 ns | 0.2384 ns |  0.94 |    0.05 | 0.0019 |      32 B |        1.33 |
