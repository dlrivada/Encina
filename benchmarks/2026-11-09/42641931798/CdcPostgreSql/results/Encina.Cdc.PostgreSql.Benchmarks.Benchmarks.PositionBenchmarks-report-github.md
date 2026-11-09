```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.314 ns | 0.0207 ns | 0.0011 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.267 ns | 0.4893 ns | 0.0268 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.245 ns | 5.2386 ns | 0.2871 ns |  1.16 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.116 ns | 4.0528 ns | 0.2221 ns |  1.14 |    0.03 | 0.0019 |      32 B |        1.33 |
