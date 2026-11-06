```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.312 ns | 0.0337 ns | 0.0018 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.342 ns | 2.1363 ns | 0.1171 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.199 ns | 0.9713 ns | 0.0532 ns |  1.14 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.376 ns | 3.6661 ns | 0.2010 ns |  1.16 |    0.03 | 0.0019 |      32 B |        1.33 |
