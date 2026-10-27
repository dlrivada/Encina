```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     39.00 ns |     8.476 ns |   0.465 ns |     1.25 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     31.28 ns |     5.200 ns |   0.285 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,108.06 ns | 8,206.278 ns | 449.814 ns | 2,913.10 |   26.25 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    133.59 ns |    18.325 ns |   1.004 ns |     4.27 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     85.20 ns |     7.708 ns |   0.422 ns |     2.72 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
