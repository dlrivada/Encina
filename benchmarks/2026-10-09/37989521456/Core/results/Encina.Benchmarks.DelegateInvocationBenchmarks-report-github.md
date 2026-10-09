```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     36.66 ns |     1.920 ns |   0.105 ns |     1.26 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.10 ns |     4.525 ns |   0.248 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,236.25 ns | 6,204.240 ns | 340.075 ns | 3,169.86 |   25.58 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    136.25 ns |     6.336 ns |   0.347 ns |     4.68 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     76.97 ns |    11.772 ns |   0.645 ns |     2.65 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
