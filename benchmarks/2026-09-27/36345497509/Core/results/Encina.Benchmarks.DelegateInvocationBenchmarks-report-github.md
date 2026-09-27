```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     36.61 ns |     2.904 ns |   0.159 ns |     1.28 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.67 ns |     3.137 ns |   0.172 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,064.57 ns | 7,856.517 ns | 430.642 ns | 3,106.71 |   20.69 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    140.25 ns |    12.255 ns |   0.672 ns |     4.89 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     77.00 ns |     2.588 ns |   0.142 ns |     2.69 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
