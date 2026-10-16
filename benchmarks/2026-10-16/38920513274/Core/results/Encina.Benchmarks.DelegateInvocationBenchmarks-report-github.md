```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     38.46 ns |     4.465 ns |   0.245 ns |     1.28 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.95 ns |     0.577 ns |   0.032 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,625.44 ns | 2,146.105 ns | 117.635 ns | 2,992.47 |    4.37 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    131.25 ns |     6.825 ns |   0.374 ns |     4.38 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.36 ns |    11.749 ns |   0.644 ns |     2.72 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
