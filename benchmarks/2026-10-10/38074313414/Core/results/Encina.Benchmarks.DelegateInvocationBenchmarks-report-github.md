```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.28 ns |     3.278 ns |   0.180 ns |     1.26 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     27.94 ns |     0.828 ns |   0.045 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 64,549.53 ns | 9,664.963 ns | 529.769 ns | 2,310.70 |   16.74 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    129.56 ns |    13.123 ns |   0.719 ns |     4.64 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     86.38 ns |    12.862 ns |   0.705 ns |     3.09 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
