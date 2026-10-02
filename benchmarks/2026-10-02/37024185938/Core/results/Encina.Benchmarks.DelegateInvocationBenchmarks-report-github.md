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
| CompiledDelegate        |     36.01 ns |     5.105 ns |   0.280 ns |     1.26 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.48 ns |     2.080 ns |   0.114 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,299.57 ns | 7,118.736 ns | 390.202 ns | 3,205.76 |   16.27 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    137.51 ns |    25.909 ns |   1.420 ns |     4.83 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     76.55 ns |     7.678 ns |   0.421 ns |     2.69 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
