```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.20 ns |    10.370 ns |   0.568 ns |     1.25 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.67 ns |     8.873 ns |   0.486 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,376.58 ns | 2,121.109 ns | 116.265 ns | 3,080.80 |   44.03 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    129.07 ns |    24.503 ns |   1.343 ns |     4.35 |    0.07 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     82.48 ns |    13.643 ns |   0.748 ns |     2.78 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
