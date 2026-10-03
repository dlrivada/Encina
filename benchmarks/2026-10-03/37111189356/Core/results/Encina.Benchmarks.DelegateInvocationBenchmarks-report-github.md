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
| CompiledDelegate        |     35.12 ns |     2.996 ns |   0.164 ns |     1.26 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     27.79 ns |     1.344 ns |   0.074 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 65,001.49 ns | 6,757.392 ns | 370.395 ns | 2,338.97 |   12.73 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    133.53 ns |    27.307 ns |   1.497 ns |     4.80 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     85.05 ns |    15.901 ns |   0.872 ns |     3.06 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
