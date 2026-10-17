```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.56 ns |    18.073 ns |   0.991 ns |     1.25 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.04 ns |     5.352 ns |   0.293 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,652.35 ns | 9,252.812 ns | 507.178 ns | 3,050.78 |   29.70 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    131.99 ns |    34.632 ns |   1.898 ns |     4.39 |    0.07 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.87 ns |     7.566 ns |   0.415 ns |     2.73 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
