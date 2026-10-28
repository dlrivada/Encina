```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     36.48 ns |     4.883 ns |   0.268 ns |     1.24 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.38 ns |     4.240 ns |   0.232 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 88,918.64 ns | 3,823.089 ns | 209.556 ns | 3,026.15 |   21.72 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    128.49 ns |     8.045 ns |   0.441 ns |     4.37 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     79.90 ns |     9.583 ns |   0.525 ns |     2.72 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
