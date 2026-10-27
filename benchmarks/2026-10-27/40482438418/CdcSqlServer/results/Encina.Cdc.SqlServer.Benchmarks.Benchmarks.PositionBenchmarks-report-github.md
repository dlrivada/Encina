```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.284 ns | 0.1224 ns | 0.0067 ns |  0.14 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 9.143 ns | 3.1971 ns | 0.1752 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.224 ns | 1.6372 ns | 0.0897 ns |  0.79 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.295 ns | 1.5291 ns | 0.0838 ns |  0.69 |    0.01 | 0.0019 |      32 B |        1.33 |
