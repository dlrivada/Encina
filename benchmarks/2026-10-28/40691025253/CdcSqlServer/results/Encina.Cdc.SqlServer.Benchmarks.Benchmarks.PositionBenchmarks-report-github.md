```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.056 ns | 0.0293 ns | 0.0016 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.106 ns | 0.4592 ns | 0.0252 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.951 ns | 2.6563 ns | 0.1456 ns |  1.17 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.579 ns | 3.0104 ns | 0.1650 ns |  1.09 |    0.03 | 0.0019 |      32 B |        1.33 |
