```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.317 ns | 0.0331 ns | 0.0018 ns |  0.23 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.792 ns | 2.5945 ns | 0.1422 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.365 ns | 6.0934 ns | 0.3340 ns |  1.27 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.535 ns | 0.1251 ns | 0.0069 ns |  1.30 |    0.03 | 0.0019 |      32 B |        1.33 |
