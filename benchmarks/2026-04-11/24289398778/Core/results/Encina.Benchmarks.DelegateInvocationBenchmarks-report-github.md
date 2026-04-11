```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.22 ns |   0.499 ns |   0.731 ns |     0.77 |    0.23 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     53.04 ns |  10.817 ns |  16.190 ns |     1.10 |    0.48 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,289.74 ns | 400.435 ns | 586.953 ns | 1,870.71 |  562.13 | 0.2441 | 0.1221 |    5283 B |       47.17 |
| GenericTypeConstruction |    130.31 ns |   2.342 ns |   3.506 ns |     2.70 |    0.81 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.20 ns |   0.494 ns |   0.740 ns |     1.68 |    0.51 | 0.0105 |      - |     176 B |        1.57 |
