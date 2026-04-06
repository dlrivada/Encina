```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| DirectCall              |     35.36 ns |     0.458 ns |   0.025 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| CompiledDelegate        |     35.93 ns |     3.613 ns |   0.198 ns |     1.02 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| MethodInfoInvoke        |     78.42 ns |     3.905 ns |   0.214 ns |     2.22 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| GenericTypeConstruction |    124.47 ns |     3.960 ns |   0.217 ns |     3.52 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| ExpressionCompilation   | 91,290.34 ns | 3,734.847 ns | 204.720 ns | 2,581.83 |    5.26 | 0.2441 | 0.1221 |    5442 B |       48.59 |
