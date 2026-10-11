```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     29.28 ns |   0.506 ns |   0.757 ns |     1.23 |    0.05 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     23.81 ns |   0.524 ns |   0.784 ns |     1.00 |    0.05 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 24,667.31 ns | 381.234 ns | 546.754 ns | 1,036.95 |   41.50 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |     93.21 ns |   1.068 ns |   1.566 ns |     3.92 |    0.15 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     63.84 ns |   2.449 ns |   3.590 ns |     2.68 |    0.17 | 0.0020 |      - |     176 B |        1.57 |
