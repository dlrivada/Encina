```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.318 ns |  0.0913 ns | 0.0050 ns |  0.20 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 6.551 ns |  3.1658 ns | 0.1735 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.211 ns |  2.4442 ns | 0.1340 ns |  1.10 |    0.03 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 9.979 ns | 15.1844 ns | 0.8323 ns |  1.52 |    0.12 | 0.0019 |      32 B |        1.33 |
