```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.65 ns |     4.063 ns |   0.223 ns |     1.24 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.78 ns |     1.416 ns |   0.078 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 68,690.24 ns | 6,207.729 ns | 340.267 ns | 2,387.09 |   11.66 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    134.43 ns |    10.465 ns |   0.574 ns |     4.67 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     86.60 ns |    16.976 ns |   0.930 ns |     3.01 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
