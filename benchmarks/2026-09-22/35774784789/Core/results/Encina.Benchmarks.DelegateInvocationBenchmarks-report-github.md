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
| CompiledDelegate        |     36.01 ns |     2.672 ns |   0.146 ns |     1.27 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.38 ns |     1.643 ns |   0.090 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,563.93 ns | 3,491.657 ns | 191.390 ns | 3,226.68 |   10.62 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    138.54 ns |     5.899 ns |   0.323 ns |     4.88 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     75.65 ns |     3.463 ns |   0.190 ns |     2.67 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
