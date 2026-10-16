```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.61 ns |     3.498 ns |   0.192 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     22.11 ns |     0.236 ns |   0.013 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 51,522.86 ns | 3,856.883 ns | 211.409 ns | 2,330.36 |    8.36 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |     99.57 ns |     4.744 ns |   0.260 ns |     4.50 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     66.35 ns |     5.102 ns |   0.280 ns |     3.00 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
