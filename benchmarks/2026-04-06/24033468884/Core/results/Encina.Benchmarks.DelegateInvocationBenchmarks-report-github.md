```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.67 ns |   0.434 ns |   0.650 ns |     1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     37.74 ns |   0.548 ns |   0.820 ns |     1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,107.86 ns | 268.637 ns | 402.083 ns | 2,362.27 |   51.58 | 0.2441 | 0.1221 |    5283 B |       47.17 |
| GenericTypeConstruction |    129.41 ns |   0.642 ns |   0.961 ns |     3.43 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     83.49 ns |   0.916 ns |   1.371 ns |     2.21 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
