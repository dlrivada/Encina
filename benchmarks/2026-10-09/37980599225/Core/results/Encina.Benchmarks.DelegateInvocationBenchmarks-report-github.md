```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.35GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     19.66 ns |    10.772 ns |   0.590 ns |     1.30 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     15.07 ns |     0.683 ns |   0.037 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 16,241.91 ns | 2,921.626 ns | 160.144 ns | 1,077.74 |    9.49 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     65.75 ns |    19.058 ns |   1.045 ns |     4.36 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     44.21 ns |    59.618 ns |   3.268 ns |     2.93 |    0.19 | 0.0105 |      - |     176 B |        1.57 |
